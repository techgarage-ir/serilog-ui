/* eslint-disable testing-library/render-result-naming-convention */
import { MantineColorScheme, MantineTheme } from '@mantine/core';
import dayjs from 'dayjs';
import { theme } from 'style/theme';
import { describe, expect, it, vi } from 'vitest';
import {
  capitalize,
  convertLogType,
  getBgLogLevel,
  printDate,
  renderCodeContent,
  splitPrintDate,
} from '../../app/util/prettyPrints';
import { AdditionalColumnLogType, LogLevel, LogType } from '../../types/types';

describe('util: pretty prints', () => {
  it.each([
    ['string', 'String'],
    ['', ''],
    [null, ''],
    [undefined, ''],
  ])('capitalize: value %s returned as %s', (entry, output) => {
    const sut = capitalize(entry);

    expect(sut).toBe(output);
  });

  describe('log level backgrounds', () => {
    it.each([
      {
        logLevelEntry: LogLevel.Verbose,
        colorScheme: 'dark',
        expectedBg: (theme: MantineTheme) => theme.colors.green[9],
      },
      {
        logLevelEntry: LogLevel.Debug,
        colorScheme: 'dark',
        expectedBg: (theme: MantineTheme) => theme.colors.blue[9],
      },
      {
        logLevelEntry: LogLevel.Information,
        colorScheme: 'dark',
        expectedBg: (theme: MantineTheme) => theme.colors.blue[6],
      },
      {
        logLevelEntry: LogLevel.Warning,
        colorScheme: 'dark',
        expectedBg: (theme: MantineTheme) => theme.colors.yellow[9],
      },
      {
        logLevelEntry: LogLevel.Error,
        colorScheme: 'dark',
        expectedBg: (theme: MantineTheme) => theme.colors.red[6],
      },
      {
        logLevelEntry: LogLevel.Fatal,
        colorScheme: 'dark',
        expectedBg: (theme: MantineTheme) => theme.colors.red[9],
      },
      {
        logLevelEntry: LogLevel.Verbose,
        colorScheme: 'light',
        expectedBg: (theme: MantineTheme) => theme.colors.green[7],
      },
      {
        logLevelEntry: LogLevel.Debug,
        colorScheme: 'light',
        expectedBg: (theme: MantineTheme) => theme.colors.blue[4],
      },
      {
        logLevelEntry: LogLevel.Information,
        colorScheme: 'light',
        expectedBg: (theme: MantineTheme) => theme.colors.blue[3],
      },
      {
        logLevelEntry: LogLevel.Warning,
        colorScheme: 'light',
        expectedBg: (theme: MantineTheme) => theme.colors.yellow[5],
      },
      {
        logLevelEntry: LogLevel.Error,
        colorScheme: 'light',
        expectedBg: (theme: MantineTheme) => theme.colors.red[4],
      },
      {
        logLevelEntry: LogLevel.Fatal,
        colorScheme: 'light',
        expectedBg: (theme: MantineTheme) => theme.colors.red[8],
      },
    ])(
      'returns color for log level $logLevelEntry and scheme $colorScheme',
      ({ logLevelEntry, colorScheme, expectedBg }) => {
        const result = getBgLogLevel(
          theme as MantineTheme,
          colorScheme as MantineColorScheme,
          logLevelEntry,
        );
        expect(result).toBe(expectedBg(theme as MantineTheme));
      },
    );

    it('returns default color for unrecognized enum case', () => {
      const themeWithDefault = {
        ...theme,
        colors: { ...theme.colors, cyan: ['#111111', '#222222'] },
      } as unknown as MantineTheme;
      const result = getBgLogLevel(themeWithDefault, 'dark', 'fakeEnum' as LogLevel);
      expect(result).toBe('#111111');
    });
  });

  describe('code content render', () => {
    it('returns xml prettified', async () => {
      const act = await renderCodeContent(
        '<root><my-xml>sample</my-xml></root>',
        LogType.Xml,
      );

      expect(act).toContain('>root</');
      expect(act).toContain('>my-xml</');
      expect(act).toContain('sample</');
    });

    it('returns json prettified', async () => {
      const act = await renderCodeContent('{ "json": "ok"}', LogType.Json);

      expect(act).toContain('>ok</');
      expect(act).toContain('>json</');
    });

    it('returns error message if content cannot be parsed', async () => {
      const consoleMock = vi.fn();
      console.warn = consoleMock;

      const actXml = await renderCodeContent('not an XML!', LogType.Xml);

      expect(actXml).toBe('not an XML!');

      const actJson = await renderCodeContent('not a JSON!', LogType.Json);

      expect(actJson).toBe('not a JSON!');

      expect(consoleMock).toHaveBeenCalledTimes(2);
    });

    it.each(['', '  '])('returns content if input: %s [invalid]', async (input) => {
      const act = await renderCodeContent(input, LogType.Json);

      expect(act).toBe(input || '');
    });

    it('returns content if type is not expected', async () => {
      const act = await renderCodeContent('{ "json": "ok"}', 'fake-content');

      expect(act).toBe('{ "json": "ok"}');
    });
  });

  describe('date prints', () => {
    it('print local date from iso string', () => {
      const time = '2022-09-27T14:15:10.000Z';
      const parsedDate = dayjs(time);

      const sut = printDate(time);

      expect(sut).toBe(`Sep 27, 2022 ${parsedDate.hour()}:15:10`);
    });

    it('print utc date from iso string', () => {
      const time = '2022-09-27T14:15:10.000Z';
      const sut = printDate(time, true);

      expect(sut).toBe(`Sep 27, 2022 14:15:10 [UTC]`);
    });

    it('return split local date from iso string', () => {
      const time = '2022-09-27T14:15:10.000Z';
      const parsedDate = dayjs(time);

      const sut = splitPrintDate(time);

      expect(sut[0]).toBe(`Sep 27, 2022`);
      expect(sut[1]).toBe(`${parsedDate.hour()}:15:10`);
    });

    it('return split utc date from iso string', () => {
      const time = '2022-09-27T14:15:10.000Z';
      const sut = splitPrintDate(time, true);

      expect(sut[0]).toBe(`Sep 27, 2022`);
      expect(sut[1]).toBe(`14:15:10 [UTC]`);
    });
  });

  it.each([
    [AdditionalColumnLogType.Json, LogType.Json],
    [AdditionalColumnLogType.Xml, LogType.Xml],
    [AdditionalColumnLogType.None, ''],
    ['some' as unknown as AdditionalColumnLogType, ''],
  ])('convert column log type %s to log type %s', (entry, output) => {
    const sut = convertLogType(entry);

    expect(sut).toBe(output);
  });
});
